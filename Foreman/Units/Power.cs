namespace Foreman.Units
{
    public readonly partial struct Power
    {
        public static Power FromKilowatts(double kilowatts)
        {
            return new(kilowatts * 1000.0);
        }

        public static Power FromMegawatts(double megawatts)
        {
            return new(megawatts * 1000.0 * 1000.0);
        }

        public static Power FromGigawatts(double gigawatts)
        {
            return new(gigawatts * 1000.0 * 1000.0 * 1000.0);
        }

        public double Kilowatts => Watts / 1000.0;

        public string ToShortString(string format)
        {
            if (Watts >= 1E9)
                return (Watts / 1E9).ToString(format) + " GW";
            if (Watts >= 1E6)
                return (Watts / 1E6).ToString(format) + " MW";
            if (Watts >= 1E3)
                return (Watts / 1E3).ToString(format) + " kW";
            return Watts.ToString(format) + " W";
        }
    }
}
