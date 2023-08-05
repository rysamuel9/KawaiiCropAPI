using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace KawaiiCrop.Controllers
{
    [ApiController]
    public class ImageController : Controller
    {
        [HttpPost("crop")]
        public IActionResult CropImage([FromForm] IFormFile imageFile, int cropX, int cropY, int width, int height)
        {
            try
            {
                if (width <= 0 || height <= 0)
                {
                    return BadRequest("Invalid width or height. Both width and height must be greater than 0.");
                }

                using var stream = new MemoryStream();
                imageFile.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);

                using var image = Image.Load(stream);

                image.Mutate(context => context.Crop(new Rectangle(cropX, cropY, width, height)));

                var resultStream = new MemoryStream();

                image.SaveAsJpeg(resultStream);

                resultStream.Seek(0, SeekOrigin.Begin);

                var fileResult = File(resultStream, "image/jpeg", "cropped_image.jpg");

                var jsonResult = new
                {
                    Message = "Image cropped successfully",
                    CroppedImage = "cropped_image.jpg"
                };

                return fileResult.CombineWith(new JsonResult(jsonResult));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the image: " + ex.Message);
            }
        }
    }

    public static class IActionResultExtensions
    {
        public static IActionResult CombineWith(this IActionResult first, IActionResult second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            return new CompositeActionResult(first, second);
        }

        private class CompositeActionResult : IActionResult
        {
            private readonly IActionResult _first;
            private readonly IActionResult _second;

            public CompositeActionResult(IActionResult first, IActionResult second)
            {
                _first = first;
                _second = second;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                await _first.ExecuteResultAsync(context);
                await _second.ExecuteResultAsync(context);
            }
        }
    }
}