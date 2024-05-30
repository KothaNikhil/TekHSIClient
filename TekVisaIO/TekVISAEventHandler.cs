namespace TekVISAIO
{
  public delegate void TekVISAEventHandler(
    VISA vi,
    TekVISADefs.EventTypes EventType,
    uint Context,
    uint UserHandle);
}
