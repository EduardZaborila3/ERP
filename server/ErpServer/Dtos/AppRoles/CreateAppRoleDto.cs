using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.AppRoles;

public record CreateAppRoleDto(
    [Required] string Name
);
