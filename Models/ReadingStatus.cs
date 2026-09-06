using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public enum ReadingStatus
{
    [Display(Name = "Хочу прочитати")]
    WantToRead = 0,

    [Display(Name = "Читаю")]
    Reading = 1,

    [Display(Name = "Прочитано")]
    Read = 2
}
