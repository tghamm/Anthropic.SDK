using System.Text.Json.Nodes;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Message = Anthropic.SDK.Messaging.Message;
using Anthropic.SDK.ComputerUse.ScreenCapture;
using Anthropic.SDK.ComputerUse.Inputs;
using Anthropic.SDK.ComputerUse.Scaling;

namespace Anthropic.SDK.ComputerUse
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Open Chrome in Incognito Mode in a given window. Then enter which Monitor Number you are using for Chrome:");
            var displayNumber = Convert.ToInt32(Console.ReadLine());
            
            IScreenCapturer capturer = new WindowsScreenCapturer();

            var (width, height) = capturer.GetScreenSize(displayNumber - 1);
            Console.WriteLine($"Screen Size: {width}x{height}");

            var coordScaler = new CoordinateScaler(true, width, height);

            var (scaledX, scaledY) = coordScaler.ScaleCoordinates(ScalingSource.COMPUTER, width, height);

            Console.WriteLine($"Scaled Screen Size: {scaledX}x{scaledY}");

            var client = new AnthropicClient();

            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>()
                {
                    new TextContent()
                    {
                        Text = """
                               Find Flights between ATL and NYC using a Google Search. 
                               Once you've searched for the flights and have viewed the initial results, 
                               switch the toggle to first class and take a screenshot of the results and tell me the price of the flights.
                               """
                    }
                }
            });
            
            var tools = new List<Common.Tool>()
            {
                new Function("computer", "computer_20241022",new Dictionary<string, object>()
                {
                    {"display_width_px", scaledX },
                    {"display_height_px", scaledY },
                    {"display_number", displayNumber }
                })
            };
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 2048,
                Model = AnthropicModels.Claude45Sonnet,
                Stream = false,
                Temperature = 0m,
                Tools = tools,
                System = new List<SystemMessage>()
                {
                    new SystemMessage($""""
                                      A Google Chrome Incognito window is already open and maximized in the appropriate monitor. Use that instance. Do not open a new instance of Google Chrome. 
                                      It is not focused, so you'll need to click on it once to focus on it.
                                      Use keyboard shortcuts to access the search bar in Google Chrome and complete your search once you've focused on the window.
                                      
                                      <SYSTEM_CAPABILITY>
                                      * You are utilising a Windows machine with internet access and Google Chrome installed.
                                      * When viewing a page it can be helpful to zoom out so that you can see everything on the page.  Either that, or make sure you scroll down to see everything before deciding something isn't available.
                                      * When using your computer function calls, they take a while to run and send back to you.  Where possible/feasible, try to chain multiple of these calls all into one function calls request.
                                      * The current date is {DateTime.Today.ToShortDateString()}.
                                      </SYSTEM_CAPABILITY>
                                      
                                      """
                                      """")
                }
            };

            var stillRunning = true;

            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            messages.Add(res.Message);
            while (stillRunning)
            {
                var toolUse = res.Content.OfType<ToolUseContent>().ToList();

                if (toolUse.Count == 0)
                {
                    stillRunning = false;
                    break;
                }
                var cb = new List<ContentBase>();
                foreach (var tool in toolUse)
                {
                    var action = tool.Input["action"].ToString();
                    var text = tool.Input["text"]?.ToString();
                    var coordinate = tool.Input["coordinate"] as JsonArray;
                    
                    switch (action)
                    {
                        case "screenshot":
                            messages.Add(new Message()
                            {
                                Role = RoleType.User,
                                Content = new List<ContentBase>()
                                {
                                    new ToolResultContent()
                                    {
                                        ToolUseId = tool.Id,
                                        Content =new List<ContentBase>() { new ImageContent()
                                        {
                                            Source = new ImageSource() {
                                                Data = DownscaleScreenshot(capturer.CaptureScreen(displayNumber -1), scaledX, scaledY),
                                                MediaType = "image/jpeg"
                                            }
                                        } }
                                    }
                                }
                            });
                            break;
                        default:
                            TakeAction(action, text,
                                coordinate == null ? null : new Tuple<int, int>(Convert.ToInt32(coordinate[0].ToString()),
                                    Convert.ToInt32(coordinate[1].ToString())), displayNumber - 1, coordScaler);
                            await Task.Delay(1000);
                            cb.Add(new ToolResultContent()
                            {
                                ToolUseId = tool.Id,
                                Content = new List<ContentBase>()
                                {
                                    new TextContent()
                                    {
                                        Text = "Action completed"
                                    }
                                }
                            });
                            break;
                    }

                }
                messages.Add(new Message()
                {
                    Role = RoleType.User,
                    Content = cb
                });
                res = await client.Messages.GetClaudeMessageAsync(parameters);
                messages.Add(res.Message);


            }


            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Final Result:");
            Console.WriteLine(messages.Last().Content.OfType<TextContent>().First().Text);
            Console.ReadLine();
        }

        public static void TakeAction(string action, string? text, Tuple<int, int>? coordinate, int monitorIndex, CoordinateScaler coordScaler)
        {
            switch (action)
            {
                case "left_click":
                    WindowsMouseController.LeftClick();
                    break;
                case "right_click":
                    WindowsMouseController.RightClick();
                    break;
                case "type":
                    KeyboardSimulator.SimulateTextInput(text);
                    break;
                case "key":
                    KeyboardSimulator.SimulateKeyCombination(text);
                    break;
                case "mouse_move":
                    var scaledCoord = coordScaler.ScaleCoordinates(ScalingSource.API, coordinate.Item1, coordinate.Item2);
                    WindowsMouseController.SetCursorPositionOnMonitor(monitorIndex, scaledCoord.Item1, scaledCoord.Item2);
                    break;
                default:
                    throw new ToolError($"Action {action} is not supported");
            }
        }


        public static string DownscaleScreenshot(byte[] screenshot, int scaledX, int scaledY)
        {
            // Convert Bitmap to MemoryStream
            using var memoryStream = new MemoryStream(screenshot);
            
            memoryStream.Position = 0; // Reset stream position

            // Load the image into ImageSharp
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);
            // Resize the image to scaled dimensions
            image.Mutate(x => x.Resize(scaledX, scaledY));

            // Save the image
            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder());
            ms.Position = 0; // Reset stream position
            //convert to byte 64 string
            byte[] imageBytes = ms.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }


    
    

    


    


    

    
}
