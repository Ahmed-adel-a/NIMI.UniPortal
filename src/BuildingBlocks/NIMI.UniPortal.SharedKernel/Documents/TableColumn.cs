namespace NIMI.UniPortal.SharedKernel.Documents;

/// <summary>A table column of a <see cref="PrintedDocument"/>: its header, width in points and alignment.</summary>
public sealed record TableColumn(string Header, double Width, bool AlignRight = false);
