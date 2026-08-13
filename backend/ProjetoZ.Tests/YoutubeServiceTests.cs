using ProjetoZ.Api.Services;

namespace ProjetoZ.Tests;

public class YoutubeServiceTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=10", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtrairVideoId_FormatosValidos_ExtraiOId(string url, string idEsperado)
    {
        Assert.Equal(idEsperado, YoutubeService.ExtrairVideoId(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("não é uma url")]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://www.youtube.com/watch?v=curto")]
    [InlineData("https://www.youtube.com/channel/UCabc123")]
    public void ExtrairVideoId_FormatosInvalidos_RetornaNull(string url)
    {
        Assert.Null(YoutubeService.ExtrairVideoId(url));
    }
}
