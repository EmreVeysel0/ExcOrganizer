namespace ExcOrganizer.Data.Models
{
    public class Trip
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Destination { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Price { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int Seats { get; set; }
    }
}