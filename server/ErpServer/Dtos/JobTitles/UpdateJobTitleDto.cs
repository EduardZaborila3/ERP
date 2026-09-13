using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.JobTitles;

public record UpdateJobTitleDto(
    [Required] string Name
);
