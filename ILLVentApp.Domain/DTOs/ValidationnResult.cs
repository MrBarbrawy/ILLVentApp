namespace ILLVentApp.Domain.DTOs
{
    public class ValidationnResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        
        public static ValidationnResult Successful(string message = "Validation successful")
        {
            return new ValidationnResult
            {
                Success = true,
                Message = message
            };
        }
        
        public static ValidationnResult Failed(string message)
        {
            return new ValidationnResult
            {
                Success = false,
                Message = message
            };
        }
    }
} 