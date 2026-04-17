// CA1716: The namespace name 'Promise.Lib' contains the VB.NET reserved word 'Lib'.
// This is intentional - 'Lib' is used as an abbreviation for 'Library' in this project.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Lib is used as abbreviation for Library, not a VB.NET keyword conflict in this context.", Scope = "namespace", Target = "~N:Promise.Lib")]
