namespace amorphie.token.core;

 public class DocumentBase
{
    public Guid UId { get; set; }
    public string Type { get; set; }
    public string SubjectType { get; set; }
    public string Category { get; set; }
    public string Notes { get; set; }
    public Guid DocumentId { get; set; }
    public EDocument Document { get; set; }
}

public class EDocument
{
    public Guid UId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string SubjectType { get; set; }
    public string Title { get; set; }
    public string Locale { get; set; }
    public string Mime { get; set; }
    public string State { get; set; }
    public Guid ContentUId { get; set; }
    public Content Content { get; set; }
}

public class Content
{
    public Guid UId { get; set; }
    public Guid DocumentUId { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public int Length { get; set; }
    public string Storage { get; set; }
    public string Path { get; set; }
    public string Text { get; set; }
    public string Binary { get; set; }
    public string State { get; set; }
    public int Id { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime ModifyDate { get; set; }
    public string RowVersion { get; set; }
}
