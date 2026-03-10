using FastEndpoints;

namespace Api.Endpoints.Authentication
{
    sealed public class AuthGroupEndpoints : Group
    {
        public AuthGroupEndpoints()
        {
            Configure("auth", c =>
            {
                //c.Description(d => d.WithTags("application"));
            });
        }
    }
}
