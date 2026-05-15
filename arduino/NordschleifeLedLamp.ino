#include <FastLED.h>

// Nordschleife LED lamp for SimHub ASCII commands: <MODE>,<SECTION>\n
#define LED_PIN 6
#define LED_TYPE WS2812B
#define COLOR_ORDER GRB
#define NUM_LEDS 60
#define LED_SECTIONS 12
#define BRIGHTNESS 80
#define SERIAL_BAUD 115200
#define MAX_COMMAND_LENGTH 16

CRGB leds[NUM_LEDS];

// Default 60 LED layout: 12 equal sections with 5 LEDs each.
// Edit these arrays if your physical Nordschleife shape uses uneven LED counts.
const uint16_t sectionStart[LED_SECTIONS] = {0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55};
const uint16_t sectionEnd[LED_SECTIONS]   = {4, 9, 14, 19, 24, 29, 34, 39, 44, 49, 54, 59};

char inputBuffer[MAX_COMMAND_LENGTH];
uint8_t inputLength = 0;

void setup() {
  Serial.begin(SERIAL_BAUD);

  FastLED.addLeds<LED_TYPE, LED_PIN, COLOR_ORDER>(leds, NUM_LEDS);
  FastLED.setBrightness(BRIGHTNESS);

  runStartupTest();
  showOffline();
}

void loop() {
  readSerialLines();
}

void readSerialLines() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();

    if (c == '\r') {
      continue;
    }

    if (c == '\n') {
      inputBuffer[inputLength] = '\0';
      handleCommand(inputBuffer);
      inputLength = 0;
      continue;
    }

    if (inputLength < MAX_COMMAND_LENGTH - 1) {
      inputBuffer[inputLength++] = c;
    } else {
      // Command too long; discard it safely and wait for the next line.
      inputLength = 0;
    }
  }
}

void handleCommand(const char* command) {
  char mode;
  int section;

  if (!parseCommand(command, mode, section)) {
    return;
  }

  renderModeAndSection(mode, section);
}

bool parseCommand(const char* command, char& mode, int& section) {
  if (command == NULL) {
    return false;
  }

  // Expected format is exactly like R,7 or P,10. Whitespace is ignored around the line by atoi.
  mode = command[0];
  if (!isValidMode(mode) || command[1] != ',') {
    return false;
  }

  const char* sectionText = command + 2;
  if (!isDigit(sectionText[0])) {
    return false;
  }

  for (uint8_t i = 0; sectionText[i] != '\0'; i++) {
    if (!isDigit(sectionText[i])) {
      return false;
    }
  }

  section = atoi(sectionText);
  return section >= 0 && section < LED_SECTIONS;
}

bool isValidMode(char mode) {
  return mode == 'R' || mode == 'Q' || mode == 'P';
}

void renderModeAndSection(char mode, int activeSection) {
  CRGB baseColor = getBaseColor(mode);

  fill_solid(leds, NUM_LEDS, baseColor);
  paintSection(activeSection, CRGB::Blue);
  FastLED.show();
}

CRGB getBaseColor(char mode) {
  switch (mode) {
    case 'R':
      return CRGB::Red;
    case 'Q':
      return CRGB::Yellow;
    case 'P':
      return CRGB::Green;
    default:
      return CRGB(8, 8, 8); // Unknown/offline fallback: dim white.
  }
}

void paintSection(int section, CRGB color) {
  if (section < 0 || section >= LED_SECTIONS) {
    return;
  }

  uint16_t startLed = sectionStart[section];
  uint16_t endLed = sectionEnd[section];

  if (startLed >= NUM_LEDS) {
    return;
  }

  if (endLed >= NUM_LEDS) {
    endLed = NUM_LEDS - 1;
  }

  for (uint16_t i = startLed; i <= endLed; i++) {
    leds[i] = color;
  }
}

void runStartupTest() {
  showSolid(CRGB::Red, 180);
  showSolid(CRGB::Yellow, 180);
  showSolid(CRGB::Green, 180);
  showSolid(CRGB::Blue, 180);
  showSolid(CRGB::Black, 100);
}

void showOffline() {
  fill_solid(leds, NUM_LEDS, CRGB(8, 8, 8));
  FastLED.show();
}

void showSolid(CRGB color, uint16_t durationMs) {
  fill_solid(leds, NUM_LEDS, color);
  FastLED.show();
  delay(durationMs);
}
