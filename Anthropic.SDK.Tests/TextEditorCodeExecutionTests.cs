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

    [TestMethod]
    public void TestTextEditorViewResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_view_result"",
  ""content"": ""function example() {\n  return 'Hello, World!';\n}"",
  ""file_type"": ""text"",
  ""num_lines"": 3,
  ""start_line"": 1,
  ""total_lines"": 100
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionViewResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_view_result, content.Type);
        Assert.AreEqual("function example() {\n  return 'Hello, World!';\n}", content.Content);
        Assert.AreEqual("text", content.FileType);
        Assert.AreEqual(3, content.NumLines);
        Assert.AreEqual(1, content.StartLine);
        Assert.AreEqual(100, content.TotalLines);
    }

    [TestMethod]
    public void TestTextEditorViewResultWithNullableFieldsDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_view_result"",
  ""content"": ""Full file content"",
  ""file_type"": ""pdf""
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionViewResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_view_result, content.Type);
        Assert.AreEqual("Full file content", content.Content);
        Assert.AreEqual("pdf", content.FileType);
        Assert.IsNull(content.NumLines);
        Assert.IsNull(content.StartLine);
        Assert.IsNull(content.TotalLines);
    }

    [TestMethod]
    public void TestTextEditorCreateResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_create_result"",
  ""is_file_update"": false
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionCreateResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_create_result, content.Type);
        Assert.AreEqual(false, content.IsFileUpdate);
    }

    [TestMethod]
    public void TestTextEditorCreateResultFileUpdateDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_create_result"",
  ""is_file_update"": true
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionCreateResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_create_result, content.Type);
        Assert.AreEqual(true, content.IsFileUpdate);
    }

    [TestMethod]
    public void TestTextEditorStrReplaceResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_str_replace_result"",
  ""old_start"": 5,
  ""old_lines"": 2,
  ""new_start"": 5,
  ""new_lines"": 3,
  ""lines"": [
    ""-    old line 1"",
    ""-    old line 2"",
    ""+    new line 1"",
    ""+    new line 2"",
    ""+    new line 3""
  ]
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionStrReplaceResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_str_replace_result, content.Type);
        Assert.AreEqual(5, content.OldStart);
        Assert.AreEqual(2, content.OldLines);
        Assert.AreEqual(5, content.NewStart);
        Assert.AreEqual(3, content.NewLines);
        Assert.IsNotNull(content.Lines);
        Assert.AreEqual(5, content.Lines.Count);
        Assert.AreEqual("-    old line 1", content.Lines[0]);
        Assert.AreEqual("+    new line 3", content.Lines[4]);
    }

    [TestMethod]
    public void TestTextEditorStrReplaceResultWithNullsDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_str_replace_result"",
  ""old_start"": null,
  ""old_lines"": null,
  ""new_start"": null,
  ""new_lines"": null,
  ""lines"": null
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionStrReplaceResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_str_replace_result, content.Type);
        Assert.IsNull(content.OldStart);
        Assert.IsNull(content.OldLines);
        Assert.IsNull(content.NewStart);
        Assert.IsNull(content.NewLines);
        Assert.IsNull(content.Lines);
    }

    [TestMethod]
    public void TestTextEditorToolResultErrorWithMessageDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result_error"",
  ""error_code"": ""file_not_found"",
  ""error_message"": ""The specified file does not exist""
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultErrorContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result_error, content.Type);
        Assert.AreEqual("file_not_found", content.ErrorCode);
        Assert.AreEqual("The specified file does not exist", content.ErrorMessage);
    }

    [TestMethod]
    public void TestTextEditorToolResultWithCreateResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result"",
  ""tool_use_id"": ""srvtoolu_01ABC123"",
  ""content"": {
    ""type"": ""text_editor_code_execution_create_result"",
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
        Assert.AreEqual("srvtoolu_01ABC123", content.ToolUseId);
        Assert.IsNotNull(content.Content);
        
        var createResult = content.Content as TextEditorCodeExecutionCreateResultContent;
        Assert.IsNotNull(createResult);
        Assert.AreEqual(ContentType.text_editor_code_execution_create_result, createResult.Type);
        Assert.AreEqual(false, createResult.IsFileUpdate);
    }

    [TestMethod]
    public void TestTextEditorToolResultWithStrReplaceResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result"",
  ""tool_use_id"": ""srvtoolu_01XYZ789"",
  ""content"": {
    ""type"": ""text_editor_code_execution_str_replace_result"",
    ""old_start"": 10,
    ""old_lines"": 1,
    ""new_start"": 10,
    ""new_lines"": 1,
    ""lines"": [""-  old content"", ""+  new content""]
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result, content.Type);
        Assert.AreEqual("srvtoolu_01XYZ789", content.ToolUseId);
        Assert.IsNotNull(content.Content);
        
        var strReplaceResult = content.Content as TextEditorCodeExecutionStrReplaceResultContent;
        Assert.IsNotNull(strReplaceResult);
        Assert.AreEqual(ContentType.text_editor_code_execution_str_replace_result, strReplaceResult.Type);
        Assert.AreEqual(10, strReplaceResult.OldStart);
        Assert.AreEqual(1, strReplaceResult.OldLines);
        Assert.AreEqual(10, strReplaceResult.NewStart);
        Assert.AreEqual(1, strReplaceResult.NewLines);
        Assert.IsNotNull(strReplaceResult.Lines);
        Assert.AreEqual(2, strReplaceResult.Lines.Count);
    }

    [TestMethod]
    public void TestTextEditorToolResultWithViewResultDeserialization()
    {
        var json = @"{
  ""type"": ""text_editor_code_execution_tool_result"",
  ""tool_use_id"": ""srvtoolu_01VIEW123"",
  ""content"": {
    ""type"": ""text_editor_code_execution_view_result"",
    ""content"": ""line 1\nline 2\nline 3"",
    ""file_type"": ""text"",
    ""num_lines"": 3,
    ""start_line"": 1,
    ""total_lines"": 100
  }
}";

        var options = new JsonSerializerOptions
        {
            Converters = { ContentConverter.Instance }
        };

        var content = JsonSerializer.Deserialize<TextEditorCodeExecutionToolResultContent>(json, options);
        
        Assert.IsNotNull(content);
        Assert.AreEqual(ContentType.text_editor_code_execution_tool_result, content.Type);
        Assert.AreEqual("srvtoolu_01VIEW123", content.ToolUseId);
        Assert.IsNotNull(content.Content);
        
        var viewResult = content.Content as TextEditorCodeExecutionViewResultContent;
        Assert.IsNotNull(viewResult);
        Assert.AreEqual(ContentType.text_editor_code_execution_view_result, viewResult.Type);
        Assert.AreEqual("line 1\nline 2\nline 3", viewResult.Content);
        Assert.AreEqual("text", viewResult.FileType);
        Assert.AreEqual(3, viewResult.NumLines);
        Assert.AreEqual(1, viewResult.StartLine);
        Assert.AreEqual(100, viewResult.TotalLines);
    }
}
