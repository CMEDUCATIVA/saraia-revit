// Copyright (c) 2026 SquareZero Inc. - Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Bibim.Core
{
    /// <summary>
    /// DeepSeek provider. DeepSeek exposes an OpenAI-compatible Chat Completions API
    /// at https://api.deepseek.com, so we reuse the LocalProvider translation layer
    /// instead of OpenAIProvider, which targets OpenAI's Responses API.
    /// </summary>
    public class DeepSeekProvider : ILlmProvider
    {
        private const string DeepSeekBaseUrl = "https://api.deepseek.com";

        private readonly string _modelId;
        private readonly LocalProvider _inner;

        public string ProviderName => "deepseek";
        public string ModelId => _modelId;

        public DeepSeekProvider(string apiKey, string modelId, HttpClient httpClient)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("DeepSeek API key is required.", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model id is required.", nameof(modelId));

            _modelId = modelId;
            _inner = new LocalProvider(apiKey, modelId, httpClient, DeepSeekBaseUrl, modelId);
        }

        public Task<JObject> SendNonStreamingAsync(
            JArray messages,
            string systemPrompt,
            JArray tools,
            CancellationToken ct,
            int maxTokens,
            bool jsonMode = false)
        {
            return _inner.SendNonStreamingAsync(messages, systemPrompt, tools, ct, maxTokens, jsonMode);
        }

        public Task<StreamResult> SendStreamingAsync(
            JArray messages,
            string systemPrompt,
            Action<string> onTextDelta,
            CancellationToken ct,
            int maxTokens)
        {
            return _inner.SendStreamingAsync(messages, systemPrompt, onTextDelta, ct, maxTokens);
        }
    }
}
