using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Middleware
{
    public class RequestLogging
    {
        private readonly RequestDelegate _next;

        public RequestLogging(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILogger<RequestLogging> logger)
        {
            var id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation(await FormatRequest(context, id));
            await _next(context);
        }
        private async Task<string> FormatRequest(HttpContext context, string currentUserId)
        {
            HttpRequest request = context.Request;

            return $"Http Request Information: {Environment.NewLine}" +
                        $"Schema:{request.Scheme} {Environment.NewLine}" +
                        $"By: {currentUserId} {Environment.NewLine}" +
                        $"Path: {request.Path} {Environment.NewLine}" +
                        $"QueryString: {request.QueryString} {Environment.NewLine}" +
                        $"Request Body: {await GetRequestBody(request)}";
        }

        public async Task<string> GetRequestBody(HttpRequest request)
        {
            request.EnableBuffering();
            request.EnableRewind();
            using (var requestStream = new MemoryStream())
            {
                await request.Body.CopyToAsync(requestStream);
                request.Body.Seek(0, SeekOrigin.Begin);
                return ReadStreamInChunks(requestStream);
            }
        }

        private static string ReadStreamInChunks(Stream stream)
        {
            const int ReadChunkBufferLength = 1024;
            stream.Seek(0, SeekOrigin.Begin);
            string result;
            using (var textWriter = new StringWriter())
            using (var reader = new StreamReader(stream))
            {
                var readChunk = new char[ReadChunkBufferLength];
                int readChunkLength;
                //do while: is useful for the last iteration in case readChunkLength < chunkLength
                do
                {
                    readChunkLength = reader.ReadBlock(readChunk, 0, ReadChunkBufferLength);
                    textWriter.Write(readChunk, 0, readChunkLength);
                } while (readChunkLength > 0);

                result = textWriter.ToString();
            }

            return result;
        }

    }
}
