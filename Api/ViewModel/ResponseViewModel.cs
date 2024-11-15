namespace ITValet.ViewModel
{
    public class ResponseViewModel
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }
}
