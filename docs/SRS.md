# Software Requirements Specification (SRS)

## 1. Introduction

### 1.1 Purpose

This document defines the functional and non-functional requirements of **Fetchify**, a modern and lightweight download manager. It aims to guide the design, development, and maintenance phases of the project. The document is intended for developers, contributors, testers, and stakeholders of the Fetchify software.

### 1.2 Scope

Fetchify is a graphical download manager for Windows that integrates with the `aria2` download engine. It provides features such as adding download URLs, scheduling, download categorization, and history logging through an intuitive interface. The software is open-source and is designed to be a free alternative to commercial download managers like IDM.

### 1.3 Definitions, Acronyms, and Abbreviations

- **GUI**: Graphical User Interface  
- **IDM**: Internet Download Manager  
- **aria2**: A lightweight multi-source, multi-protocol command-line download utility  
- **ETA**: Estimated Time of Arrival (completion)

### 1.4 References

- [GNU General Public License v3](https://www.gnu.org/licenses/gpl-3.0.en.html)  
- [aria2 Official Manual](https://aria2.github.io/manual/en/)

---

## 2. Overall Description

### 2.1 Product Perspective

Fetchify is a standalone desktop application built with C# in Visual Studio. It uses `aria2` as its core downloading engine and interacts with it through system calls. The application processes the output from `aria2` to display progress and control operations within its GUI.

### 2.2 Product Features

- Add, pause, resume, and cancel downloads
- Schedule downloads by date and time
- Categorize downloads into user-defined folders
- Track download history and status logs
- Display download speed, progress bar, ETA
- Speed limiter for bandwidth control

### 2.3 User Classes and Characteristics

| User Type       | Characteristics                               |
|-----------------|-----------------------------------------------|
| Casual Users    | Basic users looking for GUI download control  |
| Advanced Users  | Comfortable with configuring download options |

### 2.4 Operating Environment

- Platform: Windows 10 / 11  
- Framework: .NET 6 or later  
- Dependency: `aria2c` must be available in system PATH or bundled with application  

### 2.5 Design and Implementation Constraints

- Must interface correctly with `aria2` using standard input/output
- Application must remain compliant with GPL v3 licensing
- Cross-platform support is not a priority in the initial release

### 2.6 Assumptions and Dependencies

- Internet connection is required for downloading
- Compatible version of `aria2` is present on the system

---

## 3. Functional Requirements

### 3.1 Add New Download

- The user can enter a URL
- The system validates the URL and initiates download via `aria2`

### 3.2 Pause / Resume / Cancel Downloads

- GUI must include control buttons for each download
- Interface communicates commands to `aria2` in real-time

### 3.3 View Download Progress

- Real-time updates of progress, speed, and ETA per task
- Clear visual indicators (progress bars and status labels)

### 3.4 Schedule Downloads

- Users can schedule start times via built-in scheduler
- Application triggers downloads at the set time automatically

### 3.5 Manage Download Categories

- Users can define folders for organizing downloads
- Downloads are sorted accordingly at runtime

### 3.6 Maintain Download History and Logs

- Logs are stored for each download: complete, canceled, failed
- GUI provides viewable access to history

### 3.7 Speed Limiting

- User can set a maximum bandwidth per download
- Application configures this through `aria2` parameters

---

## 4. Non-Functional Requirements

### 4.1 Performance

- Capable of handling 5–10 concurrent downloads efficiently
- UI remains responsive even under high load

### 4.2 Usability

- Clean and intuitive interface, minimal user input required
- Designed for accessibility and ease-of-use

### 4.3 Reliability

- Error handling for failed downloads and lost connections
- Graceful fallback for missing dependencies like `aria2`

### 4.4 Portability

- Initially limited to Windows OS
- Future versions may support Linux

### 4.5 Security

- Input validation for download URLs and file destinations
- No direct exposure of system commands to user input

---

## 5. Future Considerations

- Browser extensions for Chrome/Edge to capture downloads
- Native link interception and clipboard monitoring
- Multi-language support
- Optional Linux support with cross-platform builds

---

## 6. Appendices

- Survey forms for feedback (TBD)
- Changelog (TBD)

---

## 7. Approval

- **Author**: Talha Bin Tahir  
- **Date**: 18 June 2025  
