namespace ExcOrganizer.Data.Models
{
    public class TripImage
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public string ImagePath { get; set; } = "";
        public Trip? Trip { get; set; }
    }
}
