public class PhoneNumberRequest
{
    public string phone { get; set; }

    public string selectedCountry { get; set; }

    public long format { get; set; } = 1;  //return format 1 always
}