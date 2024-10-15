namespace InnoShop.Services.ProductAPI.Models.Dto
{
    // name fix Response
    public class Response<TResult>
    {
        public TResult? Result { get; set; } 
        public List<Link> Links { get; set; }
    }
}
