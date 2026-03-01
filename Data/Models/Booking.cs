namespace ExcOrganizer.Data.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        public int TripId { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public Trip? Trip { get; set; }
    }
}