namespace Luiu.ViewModels
{
    public class ReviewViewModel
    {
        public int MemoryID { get; set; }
        public string Title { get; set; } 
        public string AuthorName { get; set; }
        public string SubmitDate { get; set; }
        public string Status { get; set; }

        public List<string> Itinerary { get; set; } = new List<string>();
    }
}
