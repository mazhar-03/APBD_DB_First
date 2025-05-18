using System.ComponentModel.DataAnnotations;

namespace DBFirstApproach.API.DTOs;

public class DeviceCreationAndUpdateDto
{
    [Required] public string Name { get; set; } = null!;

    [Required] public string DeviceTypeName { get; set; } = null!;

    [Required] public bool IsEnabled { get; set; }

    [Required] public string AdditionalProperties { get; set; } = null!;
}