namespace ToneDetectorLib
{
    public class QcIIPair
    {
        public string Alias { get; set; }
        public double ToneA { get; set; }
        public double ToneB { get; set; }

        public override string ToString()
        {
            return $"{Alias}: A={ToneA} Hz, B={ToneB} Hz";
        }
    }

}
