public class MaintenanceDTO
{
    public int Id { get; set; }
    public string Beskrivelse { get; set; }
    public string OpgType { get; set; }
    public string Ansvarlig { get; set; }

    public MaintenanceDTO(int id, string beskrivelse, string opgType, string ansvarlig)
    {
        this.Id = id;
        this.Beskrivelse = beskrivelse;
        this.OpgType = opgType;
        this.Ansvarlig = ansvarlig;
    }
}
