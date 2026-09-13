using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.JobTitles;

public record CreateJobTitleDto(
    [Required] string Name
);
