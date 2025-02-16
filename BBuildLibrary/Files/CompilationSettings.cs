using System.Text.Json.Serialization;

namespace Bloodthirst.BBuild;

[Flags]
public enum ExceptionHandling 
{ 
    EHa , 
    EHs , 
    EHc , 
    EHr 
};

/// <summary>
/// Specifies how the compiler generates warnings for a given compilation.
/// </summary>
public enum WarningLevel
{
    /// <summary>
    /// Suppresses all warnings.
    /// </summary>
    W0,

    /// <summary>
    /// Displays level 1 (severe) warnings. /W1 is the default setting in the command-line compiler.
    /// </summary>
    W1,

    /// <summary>
    /// Displays level 1 and level 2 (significant) warnings.
    /// </summary>
    W2,

    /// <summary>
    /// displays level 1, level 2, and level 3 (production quality) warnings. /W3 is the default setting in the IDE.
    /// </summary>
    W3,

    /// <summary>
    /// displays level 1, level 2, and level 3 warnings, and all level 4 (informational) warnings that aren't off by default. We recommend that you use this option to provide lint-like warnings. For a new project, it may be best to use /W4 in all compilations. This option helps ensure the fewest possible hard-to-find code defects.
    /// </summary>
    W4,

    /// <summary>
    /// Displays all warnings displayed by /W4 and all other warnings that /W4 doesn't include—for example, warnings that are off by default.
    /// </summary>
    Wall,
};

/// <summary>
/// <para>The compiler options specify the type of debugging information created for your program</para>
/// <para>and whether this information is kept in object files or in a program database (PDB) file.</para>
/// </summary>
public enum DebugInformation
{
    /// <summary>
    /// By default, if no debug information format option is specified,the compiler produces no debugging information, so compilation is faster.
    /// </summary>
    None,

    /// <summary>
    /// The /Z7 option produces object files that also contain full symbolic debugging information for use with the debugger. These object files and any libraries built from them can be substantially larger than files that have no debugging information. The symbolic debugging information includes the names and types of variables, functions, and line numbers. No PDB file is produced by the compiler. However, a PDB file can still be generated from these object files or libraries if the linker is passed the /DEBUG option.
    /// </summary>
    Z7,

    /// <summary>
    /// The /Zi option produces a separate PDB file that contains all the symbolic debugging information for use with the debugger. The debugging information isn't included in the object files or executable, which makes them much smaller. Use of /Zi doesn't affect optimizations. However, /Zi does imply /debug. For more information,
    /// </summary>
    Zi,

    /// <summary>
    /// The /ZI option is similar to /Zi, but it produces a PDB file in a format that supports the Edit and Continue feature. To use Edit and Continue debugging features, you must use this option. The Edit and Continue feature is useful for developer productivity, but can cause issues in code size, performance, and compiler conformance. 
    /// </summary>
    ZI
}

public enum Sanitizers
{
    /// <summary>
    /// A powerful compiler and runtime technology to uncover hard-to-find bugs.
    /// </summary>
    AddressSanitizer,

    /// <summary>
    /// A coverage-guided fuzzing library that can be used to find bugs and crashes caused by user-provided input.
    /// </summary>
    AddressFuzzer
}

public enum LanguageStandard
{
    C11,
    C17,
    CLatest,
    Cpp11,
    Cpp14,
    Cpp17,
    Cpp20,
    Cpp23Preview,
    CppLatest
}

/// <summary>
/// The /O options control various optimizations that help you create code for maximum speed or minimum size.
/// </summary>
public enum OptimizationLevel
{
    /// <summary>
    /// sets a combination of optimizations that generate minimum size code.
    /// </summary>
    O1,

    /// <summary>
    /// sets a combination of optimizations that optimizes code for maximum speed.
    /// </summary>
    O2,

    /// <summary>
    /// controls inline function expansion.
    /// </summary>
    Ob,

    /// <summary>
    /// disables optimization, to speed compilation and simplify debugging.
    /// </summary>
    Od,

    /// <summary>
    /// generates intrinsic functions for appropriate function calls.
    /// </summary>
    Oi,

    /// <summary>
    /// tells the compiler to favor optimizations for size over optimizations for speed.
    /// </summary>
    Os,

    /// <summary>
    /// (a default setting) tells the compiler to favor optimizations for speed over optimizations for size.
    /// </summary>
    Ot,

    /// <summary>
    /// suppresses the creation of frame pointers on the call stack for quicker function calls.
    /// </summary>
    Oy,
}

public sealed class CompilationSettings
{
    public ExceptionHandling[] ExceptionHandlingOptions { get; set; } = [ExceptionHandling.EHs , ExceptionHandling.EHc];

    public WarningLevel WarningLevel { get; set; } = WarningLevel.W4;
    public bool WarningsAsError { get; set; } = false;

    public DebugInformation DebugInformation { get; set; } = DebugInformation.Zi;
    public Sanitizers[] EnabledSanitizers { get; set; } = [ Sanitizers.AddressSanitizer ];

    public LanguageStandard LanguageStandard { get; set; } = LanguageStandard.Cpp17;

    public OptimizationLevel[] OptimizationLevelOptions { get; set; } = [ OptimizationLevel.Ot ];

    /// <summary>
    /// <para>Puts the generated switch case jump tables in the .rdata section instead of alongside code in the .text section.</para>
    /// <para>This flag only applies to x64 code. This flag was introduced in Visual Studio 17.7.</para>
    /// </summary>
    public bool UseJumpTableRData { get; set; } = true;

    /// <summary>
    /// <para>Used with The /MP option, causes the compiler to create one or more copies of itself, each in a separate process. Then these instances simultaneously compile the source files. </para>
    /// <para>Must range from 1 through 65536.</para> 
    /// </summary>
    public int ProcessCount { get; set; } = 8;
}
