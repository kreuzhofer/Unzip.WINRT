Unzip.WINRT
===========

This library helps you unzipping ZIP files in your WINRT applications.

Usage:

await UnZipHelper.UnZip(targetFolder, zipSourceStream, reportUnzipProgress);

The report UnzipProgress delegate is an optional parameter. The event is called when the unzip progresses.


