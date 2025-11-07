namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class DeleteAccountResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime DeletionDate { get; set; }
    }
}
