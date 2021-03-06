using System.ComponentModel.DataAnnotations;

namespace BackendTest.Dtos;

public class MovieInDbDto
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public int Year { get; set; }
    
    public string DirectorName { get; set; }

    public List<string> Actors { get; set; }
}