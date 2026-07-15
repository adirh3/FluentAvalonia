using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Avalonia.Input;

namespace FluentAvalonia.UI.Data;

/// <summary>
/// This class is part of the ListView logic, which has been suspended for now
/// </summary>
public class DataPackage
{
    private static readonly DataFormat<string> DataPackageIdFormat =
        DataFormat.CreateStringApplicationFormat("FluentAvalonia.DataPackageId");
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object>> Payloads = new();

    /// <summary>
    /// Gets or sets the requested operation for the data object
    /// </summary>
    public DragDropEffects RequestedOperation { get; set; }

    public bool Contains(string dataFormat) =>
        _data.ContainsKey(dataFormat);

    public object Get(string dataFormat) =>
        _data.TryGetValue(dataFormat, out var value) ? value :
        throw new ArgumentException($"No data format of {dataFormat} was found in the data package");

    public IEnumerable<string> GetDataFormats() =>
        _data.Keys;

    /// <summary>
    /// Gets the data for the operation as a string
    /// </summary>
    public string GetText() =>
        Get("Text") as string;

    /// <summary>
    /// Sets string content as the data for the operation
    /// </summary>
    /// <param name="txt"></param>
    public void SetText(string txt) =>
        _data.Add("Text", txt);

    /// <summary>
    /// Sets the data for the operation with the specified format
    /// </summary>
    public void SetData(string format, object value) =>
        _data.Add(format, value);

    internal DataTransfer CreateDataTransfer(out string dataPackageId)
    {
        dataPackageId = Guid.NewGuid().ToString("N");
        Payloads[dataPackageId] = new Dictionary<string, object>(_data);

        var dataTransfer = new DataTransfer();

        if (_data.TryGetValue("Text", out object textValue) && textValue is string text)
            dataTransfer.Add(DataTransferItem.CreateText(text));

        dataTransfer.Add(DataTransferItem.Create(DataPackageIdFormat, dataPackageId));
        return dataTransfer;
    }

    internal static void ReleaseDataTransfer(string dataPackageId)
    {
        if (dataPackageId is not null)
            Payloads.TryRemove(dataPackageId, out _);
    }

    public static bool Contains(IDataTransfer dataTransfer, string format)
        => TryGetData(dataTransfer, format, out object _);

    public static object Get(IDataTransfer dataTransfer, string format)
        => TryGetData(dataTransfer, format, out object value)
            ? value
            : throw new ArgumentException($"No data format of {format} was found in the data package");

    public static bool TryGetData<T>(IDataTransfer dataTransfer, string format, out T value)
    {
        if (dataTransfer?.TryGetValue(DataPackageIdFormat) is { } dataPackageId &&
            Payloads.TryGetValue(dataPackageId, out var payload) &&
            payload.TryGetValue(format, out object rawValue) &&
            rawValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
}
