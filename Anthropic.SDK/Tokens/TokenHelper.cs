using Microsoft.ML.Tokenizers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Anthropic.SDK.Tokens
{
    /// <summary>
    /// Helper Class to Get Token Counts
    /// </summary>
    public static class TokenHelper
    {
        private static readonly Tokenizer Tokenizer;
        static TokenHelper()
        {
            var vocabFilePath =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tokens", "anthropic_vocab.json");
            var mergesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tokens", "anthropic_merges.txt");
            Tokenizer = new Tokenizer(new Bpe(vocabFilePath, mergesFilePath, null, null), RobertaPreTokenizer.Instance);
        }


        /// <summary>
        /// Gets Token Count of Input String
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int GetClaudeTokenCount(this string input)
        {
            return Tokenizer.Encode(input).Tokens.Count;
        }
    }
}
