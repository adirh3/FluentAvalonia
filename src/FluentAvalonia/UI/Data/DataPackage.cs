using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace FluentAvalonia.UI.Data;

/// <summary>
/// This class is part of the ListView logic, which has been suspended for now
/// </summary>
public class DataPackage
{
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

    private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
}
