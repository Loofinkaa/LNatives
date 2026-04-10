# LNatives 

**Lean. Fast. Foundational.**

LNatives is a curated suite of lightweight, high-performance C# libraries designed to streamline everyday low-level programming tasks. Forget hunting for external dependencies — LNatives provides a cohesive set of tools for buffer management, bit-level manipulation, and memory-efficient data storage, all written in C#.

### 🧩 Core Components

| Library | Focus | Why you need it |
| :--- | :--- | :--- |
| **LNatives.Buffers** | Zero-alloc pooling & slicing | Rent, slice, and return memory without GC pressure. Ideal for high-frequency networking or parsing. |
| **LNatives.Streams** | Bit & Byte I/O | Read/write individual bits, `VarInt`, `Half` floats, or endian-specific data with a simple stream interface. |
| **LNatives.Storage** | Packed data structures | Store multiple values in a single `ulong` (e.g., `Timestamp32\|UserId32`), `Union<T>` types, and `ValueString` builders. |