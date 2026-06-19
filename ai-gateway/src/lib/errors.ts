export class GatewayError extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly statusCode: number = 500,
    public readonly provider?: string,
  ) {
    super(message);
    this.name = 'GatewayError';
  }
}
