using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.AppRoles;

public record UpdateAppRoleDto(
    [Required] string Name
);
