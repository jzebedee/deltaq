namespace DeltaQ.BsDiff;

internal static class Constants
{
    public const int HeaderSize = 32;

    public const int HeaderOffsetSig = 0;
    public const int HeaderOffsetCtrl = sizeof(long) * 1;
    public const int HeaderOffsetDiff = sizeof(long) * 2;
    public const int HeaderOffsetNewData = sizeof(long) * 3;

    public const long Signature = 0x3034464649445342; //"BSDIFF40"
}
