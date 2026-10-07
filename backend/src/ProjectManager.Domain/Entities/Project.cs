namespace ProjectManager.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
<<<<<<<< HEAD:WebAPI/Models/Project.cs
    public bool IsDeleted { get; set; }
========
    public bool IsDeleted { get; set; } = false;
    public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
>>>>>>>> d6d0bdf2c9bc385a1f551859341e9c8735a3649f:backend/src/ProjectManager.Domain/Entities/Project.cs
}
