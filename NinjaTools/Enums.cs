namespace NinjaTools
{
    public enum DocumentType
    {
        Log         = 0,
        Trace       = 1,
        Workspace   = 2,
        Config      = 3,
        UI          = 4,
        Template    = 5,
        Database    = 6,
        UseType     = 7,
    }

    public enum FilterRowType
    {
        None        = 0,
        SystemInfo  = 1,
        Warning     = 2,
        Error       = 3,
        Time        = 4,
        Account     = 5,
        Order       = 6,
        Manual      = 7,
    }
}