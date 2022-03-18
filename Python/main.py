from pythonosc import osc_bundle_builder
from pythonosc import osc_message_builder
from pythonosc import udp_client
import asyncio
import sys

client = udp_client.SimpleUDPClient("127.0.0.1", 9000)


def send_clk_and_data(clk=True, data=True, why=""):
    if why != "":
        print(f"{why}:\t(CLK={'H' if clk else 'L'}, DATA={1 if data else 0})")
    bundle = osc_bundle_builder.OscBundleBuilder(osc_bundle_builder.IMMEDIATELY)
    clk_msg = osc_message_builder.OscMessageBuilder(address="/avatar/parameters/SERIAL_CLK")
    clk_msg.add_arg(clk)
    data_msg = osc_message_builder.OscMessageBuilder(address="/avatar/parameters/SERIAL_DATA")
    data_msg.add_arg(data)
    bundle.add_content(clk_msg.build())
    bundle.add_content(data_msg.build())
    client.send(bundle.build())


# TODO: This could have been just a simple bool array or something
def serial_message(data_to_transmit: int):
    number_of_bits = 16
    # data_to_transmit = 6 * 60 + 45  # The time is 6:45

    yield True  # Idle
    yield False  # Sync bit
    for i in range(number_of_bits):  # LSB first
        yield (data_to_transmit >> i & 1) == 1  # Bit #i
    yield True  # Parity bit
    yield True  # End bit


# TODO: this is hot garbage, I need a Python idiom reviewer
class SerialComms:
    IDLE = 0
    QUEUED = 1

    message_pushed_event = asyncio.Future()
    msg = -1
    state = IDLE

    async def clk_routine(self, clk_speed_seconds):
        clk = False
        while True:
            if self.state == self.IDLE:
                send_clk_and_data(clk, True)  # Idle
                clk = not clk
                await asyncio.sleep(clk_speed_seconds)
            else:
                for data in serial_message(self.msg):
                    send_clk_and_data(clk, data, "Serial")  # Serial
                    clk = not clk
                    await asyncio.sleep(clk_speed_seconds)
                self.message_pushed_event.set_result(True)
                self.state = self.IDLE

    async def push_message(self, message):
        self.msg = message
        self.state = self.QUEUED
        self.message_pushed_event = asyncio.get_running_loop().create_future()
        await self.message_pushed_event


async def get_user_input(msg):
    await asyncio.get_event_loop().run_in_executor(None, lambda s=msg: sys.stdout.write(f"{s} "))
    return await asyncio.get_event_loop().run_in_executor(None, sys.stdin.readline)


serial = SerialComms()


async def user_input():
    while True:
        line = await get_user_input("Transmit:")
        await serial.push_message(int(line))


async def main():
    task = asyncio.create_task(serial.clk_routine(1 / 4))
    task2 = asyncio.create_task(user_input())
    await task
    await task2


asyncio.run(main())
