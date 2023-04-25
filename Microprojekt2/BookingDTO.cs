public class BookingDTO
{
    public string KundeNavn { get; set; }
    public DateTime StartTidspunkt { get; set; }
    public string StartSted { get; set; }
    public string SlutSted { get; set; }

    public BookingDTO(string kundeNavn, DateTime startTidspunkt, string startSted, string slutSted)
    {
        this.KundeNavn = kundeNavn;
        this.StartTidspunkt = startTidspunkt;
        this.StartSted = startSted;
        this.SlutSted = slutSted;
    }
}
