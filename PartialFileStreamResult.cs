using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

public class PartialFileStreamResult : FileResult
{
#nullable disable
  private Stream _fileStream;
#nullable enable
  public PartialFileStreamResult(Stream fileStream, string contentType, string? downloadFileName)
    : this(fileStream, MediaTypeHeaderValue.Parse((StringSegment)contentType), downloadFileName) { }

  private PartialFileStreamResult(Stream fileStream, MediaTypeHeaderValue contentType, string? downloadFileName)
    : base(contentType.ToString()) {
    FileStream = fileStream != null ? fileStream : throw new ArgumentNullException(nameof(fileStream));
    FileDownloadName = downloadFileName;
  }

  private Stream FileStream
  {
    get => _fileStream;
    [MemberNotNull("_fileStream")]
    set => _fileStream = value != null ? value : throw new ArgumentNullException(nameof(value));
  }

  private class StubLoggerFactory : ILoggerFactory
  {
    private class StubLogger : ILogger, IDisposable
    {
      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception, string> formatter) {
        if (exception is not null)
          Console.WriteLine(formatter(state, exception));
      }

      public bool IsEnabled(LogLevel logLevel) => true;

      public IDisposable BeginScope<TState>(TState state) => this;

      public void Dispose() { }
    }

    public void Dispose() { }

    public void AddProvider(ILoggerProvider provider) { }

    public ILogger CreateLogger(string categoryName) => new StubLogger();

  }

  public override Task ExecuteResultAsync(ActionContext context) {
    if (context == null)
      throw new ArgumentNullException(nameof(context));

    var executor = new PartialFileStreamResultExecutor(new StubLoggerFactory());
    return executor.ExecuteAsync(context, this);
  }

  private class PartialFileStreamResultExecutor : FileResultExecutorBase
  {
    public PartialFileStreamResultExecutor(ILoggerFactory loggerFactory)
      : base(CreateLogger<PartialFileStreamResultExecutor>(loggerFactory)) { }

    public async Task ExecuteAsync(ActionContext context, PartialFileStreamResult result) {
      if (context == null)
        throw new ArgumentNullException(nameof(context));
      if (result == null)
        throw new ArgumentNullException(nameof(result));

      await using (result.FileStream) {
        var fileLength = new long?();

        // Let here filestream to start not from the beginning -- that's why we made this class in a first place :)
        if (result.FileStream.CanSeek)
          fileLength = result.FileStream.Length - result.FileStream.Position;

        var (range, rangeLength, serveBody) = SetHeadersAndLog(context, result, fileLength,
          result.EnableRangeProcessing, result.LastModified, result.EntityTag);

        if (!serveBody)
          return;

        await WriteFileAsync(context, result, range, rangeLength);
      }
    }

    private Task WriteFileAsync(ActionContext context, PartialFileStreamResult result, RangeItemHeaderValue? range,
      long rangeLength) {
      if (context == null)
        throw new ArgumentNullException(nameof(context));

      if (result == null)
        throw new ArgumentNullException(nameof(result));

      if (range != null && rangeLength == 0L)
        return Task.CompletedTask;

      return FileResultExecutorBase.WriteFileAsync(context.HttpContext, result.FileStream, range, rangeLength);
    }
  }
}
