namespace Foreman.Units
{
    public partial struct Power
    {
        public static Power FromKilowatts(double kilowatts)
        {
            return new Power(kilowatts * 1000.0);
        }

        public double Kilowatts => Watts / 1000.0;
    }
}
