namespace MudskipDB.Dto
{
    public class ReviewDTO
    {
        public string Username { get; set; }  // A felhasználó neve
        public string Comment { get; set; }   // A vélemény szövege
        public int Rating { get; set; }       // Az értékelés pontszáma (pl. 1-5 között)
       
    }
}
