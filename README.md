# AutoDecoder - Log Line Decoder

A comprehensive C# WinForms application demonstrating Object-Oriented Programming principles including inheritance, encapsulation, and polymorphism through automatic decoding of automotive diagnostic log files.

## Project Structure

### 1. AutoDecoder.Models (Class Library)
Contains the core OOP architecture with abstract base class and derived types.

**Files:**
- `LineType.cs` - Enumeration of supported line types (Iso15765, Xml, Hex, Ascii, Unknown)
- `LogLine.cs` - Abstract base class demonstrating inheritance and encapsulation
- `Iso15765Line.cs` - Derived class for ISO 15765 CAN diagnostic protocol lines
- `XmlLine.cs` - Derived class for XML formatted lines with DID information
- `HexLine.cs` - Derived class for raw hexadecimal data
- `AsciiLine.cs` - Derived class for plain ASCII text
- `UnknownLine.cs` - Derived class for unrecognized formats

**OOP Concepts Demonstrated:**
- **Inheritance**: All line types inherit from abstract `LogLine` base class
- **Encapsulation**: Private fields (`_raw`, `_lineNumber`) with public read-only properties
- **Polymorphism**: Abstract `ParseAndDecode()` method implemented differently by each derived class
- **Abstraction**: Abstract `Type` property forces derived classes to specify their type

### 2. AutoDecoder.Decoders (Class Library)
Contains deterministic decoding logic and helper utilities.

**Files:**
- `DecodeResult.cs` - Data class for decode results (Summary, Details, Confidence)
- `HexTools.cs` - Static helper methods for parsing hex data
- `DecodeTables.cs` - Static lookup tables for UDS services, NRCs, and DIDs
- `UdsDecoder.cs` - Decodes Unified Diagnostic Services (UDS) protocol messages
- `Iso15765Decoder.cs` - Decodes ISO 15765 CAN diagnostic protocol
- `XmlDidDecoder.cs` - Extracts and decodes DID information from XML
- `LineClassifier.cs` - Classifies raw lines into appropriate LogLine types

**Supported Patterns:**

#### A) ISO 15765 Lines
```
ISO15765 TX -> ... [00,00,07,D0,22,80,6A]
ISO15765 RX <- ... [00,00,07,D8,7F,22,78]
```
- Extracts direction (TX/RX) and payload bytes
- Decodes UDS messages within payload
- Handles negative responses (0x7F) and positive responses (0x62)

#### B) UDS Protocol
- **Negative Response (0x7F)**: `0x7F <ServiceID> <NRC>`
  - Example: `7F 22 78` = Negative Response to ReadDataByIdentifier, Response Pending
- **Positive Response (0x62)**: `0x62 <DID_Hi> <DID_Lo> <Data...>`
  - Example: `62 F1 88 ...` = Positive Response, DID 0xF188 (Strategy)

#### C) XML Lines
```xml
<ns3:didValue didValue="F188"><ns3:Response>4D59...</ns3:Response></ns3:didValue>
```
- Extracts DID values and response data
- Identifies known DIDs (F188, F110, F111, F113, F124, DE00)

#### D) Hex Lines
```
[48,65,6C,6C,6F]
DEADBEEF01234567
```
- Parses bracket notation or continuous hex strings
- Provides ASCII preview

#### E) ASCII Lines
Plain text entries with high percentage of printable characters

### 3. AutoDecoder.Gui (WinForms Application)
User interface for loading and viewing decoded log files.

**Features:**
- **Load File**: Open and decode any text/log file
- **Load Sample**: Load 50+ pre-configured sample lines demonstrating all formats
- **Clear**: Reset all data
- **DataGridView**: Displays all decoded lines with columns for each property
- **Split View**: Raw line (left) and decoded details (right)

**Error Handling:**
- All file operations wrapped in try-catch blocks
- Failed line decoding creates UnknownLine objects instead of crashing
- User-friendly MessageBox alerts for errors

## Usage

1. **Build and Run**: Open the solution in Visual Studio and run (F5)
2. **Load Sample**: Click "Load Sample" to see 50+ demonstration lines
3. **Load File**: Click "Load File" to open your own log file
4. **View Details**: Click any row in the grid to see raw and decoded views

## OOP Requirements Met

✅ **10+ Objects**: Each log line becomes an object; sample includes 50+ lines  
✅ **Inheritance**: Abstract `LogLine` base class with 5 derived types  
✅ **Encapsulation**: Private fields with public read-only properties  
✅ **Line-by-Line Comments**: Every line has a comment explaining its purpose  
✅ **Error Handling**: No crashes on bad input; all exceptions caught and displayed  

## Supported UDS Services
- 0x10 DiagnosticSessionControl
- 0x11 ECUReset
- 0x22 ReadDataByIdentifier
- 0x27 SecurityAccess
- 0x2E WriteDataByIdentifier
- 0x31 RoutineControl
- 0x3E TesterPresent
- 0x7F NegativeResponse

## Supported NRCs (Negative Response Codes)
- 0x10 GeneralReject
- 0x11 ServiceNotSupported
- 0x12 SubFunctionNotSupported
- 0x13 IncorrectMessageLengthOrInvalidFormat
- 0x22 ConditionsNotCorrect
- 0x31 RequestOutOfRange
- 0x33 SecurityAccessDenied
- 0x35 InvalidKey
- 0x36 ExceededNumberOfAttempts
- 0x37 RequiredTimeDelayNotExpired
- 0x78 ResponsePending (most common)

## Supported DIDs (Data Identifiers)
- 0xF188 Strategy
- 0xF110 PartII_Spec
- 0xF111 CoreAssembly
- 0xF113 Assembly
- 0xF124 Calibration
- 0xDE00 DirectConfiguration

## Technical Details

**Target Framework**: .NET 10.0  
**Language**: C# with nullable reference types enabled  
**Architecture**: 3-tier (Models, Business Logic/Decoders, Presentation/GUI)  
**Project References**:
- Gui → Models + Decoders
- Decoders → Models
