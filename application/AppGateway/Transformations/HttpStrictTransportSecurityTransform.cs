using Yarp.ReverseProxy.Transforms;

namespace AppGateway.Transformations;

public class HttpStrictTransportSecurityTransform()
    : ResponseHeaderValueTransform("Strict-Transport-Security", "max-age=2592000", false, ResponseCondition.Success);
