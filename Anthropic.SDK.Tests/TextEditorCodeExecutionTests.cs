using System.Text.Json;
using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests;

[TestClass]
public class TextEditorCodeExecutionTests
{
    [TestMethod]
    public void TestTextEditorToolUseDeserialization()
    {
        var json = @"{
  ""type"": ""server_tool_use"",
  ""id"": ""srvtoolu_01E6F7G8H9I0J1K2L3M4N5O6"",
  ""name"": ""text_editor_code_execution"",
  ""input"": {
    ""command"": ""str_replace"",
    ""path"": ""config.json"",
    ""old_str"": ""\""debug\"": true"",
    ""new_str"": ""\""debug\"": false""
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<ServerToolUseContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.server_tool_use, content.Type);
        Assert.AreEqual("srvtoolu_01E6F7G8H9I0J1K2L3M4N5O6", content.Id);
        Assert.AreEqual("text_editor_code_execution", content.Name);
        Assert.IsNotNull(content.Input);
        Assert.AreEqual("str_replace", content.Input.Command);
        Assert.AreEqual("config.json", content.Input.Path);
        Assert.AreEqual("\"debug\": true", content.Input.OldStr);
        Assert.AreEqual("\"debug\": false", content.Input.NewStr);
    }

    [TestMethod]
    public void TestTextEditorToolResultWithDiffDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result"",
  ""tool_use_id"": ""srvtoolu_01E6F7G8H9I0J1K2L3M4N5O6"",
  ""content"": {
    ""type"": ""text_editor_code_execution_result"",
    ""oldStart"": 3,
    ""oldLines"": 1,
    ""newStart"": 3,
    ""newLines"": 1,
    ""lines"": [""-  \""debug\"": true"", ""+  \""debug\"": false""]
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result, content.Type);
        Assert.AreEqual("srvtoolu_01E6F7G8H9I0J1K2L3M4N5O6", content.ToolUseId);
        Assert.IsNotNull(content.Content);
        
        var result = content.Content as TextEditorCodeExecutionResultContent;
        Assert.IsNotNull(result);
        Assert.AreEqual(ContentType.text_editor_code_execution_result, result.Type);
        Assert.AreEqual(3, result.OldStart);
        Assert.AreEqual(1, result.OldLines);
        Assert.AreEqual(3, result.NewStart);
        Assert.AreEqual(1, result.NewLines);
        Assert.IsNotNull(result.Lines);
        Assert.AreEqual(2, result.Lines.Count);
        Assert.AreEqual("-  \"debug\": true", result.Lines[0]);
        Assert.AreEqual("+  \"debug\": false", result.Lines[1]);
    }

    [TestMethod]
    public void TestTextEditorCreateToolResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result"",
  ""tool_use_id"": ""srvtoolu_01D5E6F7G8H9I0J1K2L3M4N5"",
  ""content"": {
    ""type"": ""text_editor_code_execution_result"",
    ""is_file_update"": false
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result, content.Type);
        Assert.AreEqual("srvtoolu_01D5E6F7G8H9I0J1K2L3M4N5", content.ToolUseId);
        Assert.IsNotNull(content.Content);
        
        var result = content.Content as TextEditorCodeExecutionResultContent;
        Assert.IsNotNull(result);
        Assert.AreEqual(ContentType.text_editor_code_execution_result, result.Type);
        Assert.AreEqual(false, result.IsFileUpdate);
    }

    [TestMethod]
    public void TestTextEditorToolResultErrorDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result"",
  ""tool_use_id"": ""srvtoolu_01VfmxgZ46TiHbmXgy928hQR"",
  ""content"": {
    ""type"": ""text_editor_code_execution_tool_result_error"",
    ""error_code"": ""unavailable""
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result, content.Type);
        Assert.AreEqual("srvtoolu_01VfmxgZ46TiHbmXgy928hQR", content.ToolUseId);
        Assert.IsNotNull(content.Content);
        
        var error = content.Content as TextEditorCodeExecutionToolResultErrorContent;
        Assert.IsNotNull(error);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result_error, error.Type);
        Assert.AreEqual("unavailable", error.ErrorCode);
    }

    [TestMethod]
    public void TestPauseTurnStopReason()
    {
        var json = @"{
  ""id"": ""msg_01XFDUDYJgAACzvnptvVoYEL"",
  ""type"": ""message"",
  ""role"": ""assistant"",
  ""content"": [{""type"": ""text"", ""text"": ""Processing...""}],
  ""model"": ""claude-3-5-sonnet-20241022"",
  ""stop_reason"": ""pause_turn"",
  ""stop_sequence"": null,
  ""usage"": {
    ""input_tokens"": 10,
    ""output_tokens"": 20
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var response = JsonSerializer.Deserialize<MessageResponse>(json, options);
        
        Assert.IsNotNull(response);
        Assert.AreEqual("pause_turn", response.StopReason);
        Assert.AreEqual("msg_01XFDUDYJgAACzvnptvVoYEL", response.Id);
        Assert.AreEqual("assistant", response.Role.ToString().ToLower());
    }
}
