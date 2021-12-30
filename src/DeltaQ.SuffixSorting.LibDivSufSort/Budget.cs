namespace DeltaQ.SuffixSorting.LibDivSufSort;
internal ref struct Budget
{
    public int Chance;
    public int Remain;
    public int IncVal;
    public int Count;

    public Budget(int chance, int incVal)
    {
        Chance = chance;
        Remain = incVal;
        IncVal = incVal;
        Count = 0;
    }

    public bool Check(int size)
    {
        if (size <= Remain)
        {
            Remain -= size;
            return true;
        }

        if (Chance == 0)
        {
            Count += size;
            return false;
        }

        Remain += IncVal - size;
        Chance -= 1;
        return true;
    }
}