Roslyn Clr Heap Allocation Analyzer
===================================

This is a fork from the original project at [Clr Heap Allocation Analyzer](https://github.com/Microsoft/RoslynClrHeapAllocationAnalyzer).

This project contains a number of changes and difference in functionality
compared to the original project, including:
* The ability to mark methods with attributes so that only the marked methods
  are analyzed for allocations ("hot path analysis").
* Settings to control file patterns and attributes for analysis exclusion
* The ability to change the severities of the different rules
* Removal of the HeapAllocationAnalyzerEventSource

## About

Roslyn based C# heap allocation diagnostic analyzer that can detect explicit and
many implicit allocations like boxing, display classes a.k.a closures, implicit
delegate creations, etc.

# Releases

There are currently no binary releases from this project. If you want to use the
functionality, you need to download the project and build from source.