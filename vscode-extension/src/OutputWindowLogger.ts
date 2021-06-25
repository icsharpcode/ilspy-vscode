import { OutputChannel, window } from "vscode";

export default class OutputWindowLogger {
  private channel: OutputChannel;
  private dateTimeFormat: Intl.DateTimeFormat;

  constructor() {
    this.channel = window.createOutputChannel("ILSpy Extension");
    this.dateTimeFormat = new Intl.DateTimeFormat("en", {
      hour: "2-digit",
      hour12: false,
      minute: "2-digit",
      second: "2-digit",
      fractionalSecondDigits: 3,
    } as any);
  }

  public writeLine(line: string) {
    this.channel.appendLine(
      `[${this.dateTimeFormat.format(Date.now())}] ${line}`
    );
  }
}
