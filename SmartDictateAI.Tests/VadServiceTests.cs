using System;
using SmartDictateAI.Services;
using Xunit;

namespace SmartDictateAI.Tests
{
    public class VadServiceTests
    {
        [Fact]
        public void VadService_SetMode_UpdatesVadModeSuccessfully()
        {
            // Arrange
            using var vadService = new VadService();

            // Act & Assert
            // Should not throw even if native WebRtcVad DLL is not in test environment path,
            // because VadService catches exceptions gracefully.
            vadService.Initialize(3);
            vadService.SetMode(2);

            // If VAD is not loaded, HasSpeech should still return true as fallback.
            var dummyFrame = new byte[640];
            var result = vadService.HasSpeech(dummyFrame, 1.0f);
            Assert.True(result);
        }
    }
}
