import * as vscode from "vscode";

export default class OutputWindowLogger {
  private channel: vscode.OutputChannel;

  constructor() {
    this.channel = vscode.window.createOutputChannel("ILSpy Extension");
  }

  public writeLine(line: string) {
    this.channel.appendLine(line);
  }
}
