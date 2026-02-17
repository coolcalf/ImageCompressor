using System.Xml.Serialization;

namespace ImageCompressor;

[XmlRoot("UserConfig")]
public class UserConfig
{
    [XmlElement("TargetSizeTag")]
    public string TargetSizeTag { get; set; }

    [XmlElement("CustomSize")]
    public string CustomSize { get; set; }

    [XmlElement("UseBestCompression")]
    public bool UseBestCompression { get; set; }

    [XmlElement("OutputPath")]
    public string OutputPath { get; set; }
}