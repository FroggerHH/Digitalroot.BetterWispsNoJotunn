using Extensions;

namespace BetterWispsNoJotunn;

public static class ZDOExtension
{
    public static int Get_ID(this ZDO zdo)
    {
        return zdo.GetPosition().RoundCords().GetHashCode() + zdo.GetPrefab();
    }
}