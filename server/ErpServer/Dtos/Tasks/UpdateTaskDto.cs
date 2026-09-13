using System.ComponentModel.DataAnnotations;
using TaskStatus = ErpServer.Models.TaskStatus;

namespace ErpServer.Dtos.Tasks;

public record UpdateTaskDto(
    [Required(ErrorMessage = "Titlul este obligatoriu.")] 
    string Title,
    
    [Required(ErrorMessage = "Descrierea este obligatorie.")] 
    string Description,
    
    TaskStatus Status,
    
    DateTime? Deadline = null,
    
    List<int>? AssigneeIds = null
);
