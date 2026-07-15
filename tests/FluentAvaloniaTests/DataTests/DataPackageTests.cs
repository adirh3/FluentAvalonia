using Avalonia.Input;
using FluentAvalonia.UI.Data;
using Xunit;

namespace FluentAvaloniaTests.DataTests;

public class DataPackageTests
{
    [Fact]
    public void CreateDataTransfer_RoundTripsObjectPayloadAndText()
    {
        var expected = new object();
        var package = new DataPackage();
        package.SetData("TabItem", expected);
        package.SetText("tab text");

        IDataTransfer dataTransfer = package.CreateDataTransfer(out string dataPackageId);
        try
        {
            Assert.Same(expected, DataPackage.Get(dataTransfer, "TabItem"));
            Assert.True(DataPackage.TryGetData(dataTransfer, "TabItem", out object actual));
            Assert.Same(expected, actual);
            Assert.Equal("tab text", dataTransfer.TryGetText());
        }
        finally
        {
            DataPackage.ReleaseDataTransfer(dataPackageId);
            dataTransfer.Dispose();
        }

        Assert.False(DataPackage.Contains(dataTransfer, "TabItem"));
    }
}
