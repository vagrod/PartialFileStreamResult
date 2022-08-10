# PartialFileStreamResult
(.net 6) Implementation of "FileResult" that allows you to use FileStream that was seek'd to any point of a binary blob without getting the "incomplete download" error

You can use it just like your familiar `FileStreamResult`, which is:

``` cs
var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

return new FileStreamResult(stream, mimeType);
```

But with `PartialFileStreamResult` you can do `Seek` beforehand, and it still will be totally ok:

``` cs
var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

stream.Seek(bytesToSkip, SeekOrigin.Begin);

return new PartialFileStreamResult(stream, mimeType, fileName);
```

Notice `fileName` which you can also pass. It's nullable, so you don't have to.

Enjoy :))

Link to the original `FileStreamResult` source: [here](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Core/src/FileStreamResult.cs) (MIT)
