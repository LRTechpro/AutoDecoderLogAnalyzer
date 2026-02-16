namespace AutoDecoder.Models;

// Enumeration representing the different types of log lines that can be decoded
public enum LineType
{
    // ISO 15765 protocol lines (CAN diagnostic protocol)
    Iso15765,
    // XML formatted lines containing DID information
    Xml,
    // Raw hexadecimal data lines
    Hex,
    // Plain ASCII text lines
    Ascii,
    // Unrecognized or unparseable lines
    Unknown
}
